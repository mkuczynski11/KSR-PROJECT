<template>
  <b-container>
    <b-row v-if="booksLoaded" class="py-4">
      <b-col cols="1"></b-col>
      <b-col>
        <b-table
          cols="10"
          v-if="booksLoaded"
          :fields="fields"
          :items="items"
          show-empty
        >
          <template #cell(discount)="data">
            {{ data.item.discount }}%
          </template>
          <template #cell(zakup)="data">
            <b-button
              variant="success"
              @click="openBookReservationForm(data.item)"
              :disabled="data.item.quantity == 0"
              >Zamów</b-button
            >
          </template>
          <template #empty="">
            <h4><b>No books found</b></h4>
          </template>
        </b-table>
      </b-col>
      <b-col cols="1"></b-col>
    </b-row>
    <b-row v-else>
      <b-col cols="12" class="py-4">
        <b-spinner style="width: 3rem; height: 3rem" />
      </b-col>
    </b-row>
    <b-modal id="book-reservation-modal" size="lg" :hide-header="true">
      <b-container>
        <h4>Please specify the qunatity</h4>
        <hr />
        <b-row class="pt-2">
          <b-col cols="3">
            <b>Price per book</b>
          </b-col>
          <b-col cols="3" v-if="bookToReserve">
            {{ bookToReserve.unitPrice }} zł
          </b-col>
          <b-col cols="3">
            <b>Total price</b>
          </b-col>
          <b-col cols="3" v-if="bookToReserve && form.quantity">
            {{
              bookToReserve.unitPrice * parseInt(form.quantity) +
              (chosenShippingMethod != null ? chosenShippingMethod.price : 0)
            }}
            zł
          </b-col>
        </b-row>
        <b-row class="pt-2">
          <b-col cols="3">
            <b>Available discount</b>
          </b-col>
          <b-col cols="3" v-if="bookToReserve">
            {{ bookToReserve.discount }} %
          </b-col>
          <b-col cols="3">
            <b>Price after discount</b>
          </b-col>
          <b-col cols="3" v-if="bookToReserve">
            {{
              (
                bookToReserve.unitPrice *
                  parseInt(form.quantity) *
                  (1 - (bookToReserve.discount / 100)) +
                (chosenShippingMethod != null ? chosenShippingMethod.price : 0)
              ).toFixed(2)
            }}
            zł
          </b-col>
        </b-row>

        <b-row class="pt-2">
          <b-col cols="3">
            <b>Quantity</b>
          </b-col>
          <b-col cols="6">
            <b-form-input
              v-model="form.quantity"
              :type="'number'"
              placeholder="1"
            />
          </b-col>
        </b-row>
        <b-row class="pt-2">
          <b-col cols="3">
            <b>Shipping</b>
          </b-col>
          <b-col cols="6">
            <b-form-select
              v-model="chosenShippingMethod"
              :options="shippingMethods"
            />
          </b-col>
        </b-row>
      </b-container>
      <template #modal-footer="{}">
        <b-button variant="danger" @click="closeBookReservationForm()">
          Cancel
        </b-button>
        <b-button
          variant="success"
          @click="beginOrder()"
          :disabled="!validForm()"
          >Add to the basket!</b-button
        >
      </template>
    </b-modal>
  </b-container>
</template>

<script>
import {
  getBooks,
  createOrder,
  getShippingMethods,
  getBooksPrices,
  getBooksDiscounts,
} from "@/api/api.js";

const BOOK_FIELDS = ["name", "quantity", "unitPrice", "discount", "zakup"];

export default {
  name: "Books",
  data: function () {
    return {
      books: [],
      items: [],
      fields: BOOK_FIELDS,
      booksLoaded: false,
      form: {
        quantity: null,
      },
      bookToReserve: null,
      order: null,
      shippingMethods: null,
      chosenShippingMethod: null,
    };
  },

  async mounted() {
    await this.parseBooks();
    await this.parseShippingMethods();
  },

  methods: {
    async loadBooks() {
      let res = await getBooks();
      this.books = res;
    },

    async loadPrices() {
      let res = await getBooksPrices();

      return res;
    },

    async loadDiscounts() {
      let res = await getBooksDiscounts();

      return res;
    },

    async parseBooks() {
      this.booksLoaded = false;
      await this.loadBooks();
      this.items = [];

      let prices = await this.loadPrices();
      let discounts = await this.loadDiscounts();

      for (let book of this.books) {
        let newBook = JSON.parse(JSON.stringify(book));

        for (let p of prices) {
          if (p.id == book.id) {
            newBook.unitPrice = p.price;
            break;
          }
        }
        for (let d of discounts) {
          if (d.id == book.id) {
            newBook.discount = d.discount * 100;
            break;
          }
        }
        this.items.push(newBook);
      }

      this.booksLoaded = true;
    },

    async parseShippingMethods() {
      let methods = await getShippingMethods();

      this.shippingMethods = [
        {
          value: null,
          text: "Choose shipping method",
        },
      ];

      for (let method of methods) {
        let newMethod = JSON.parse(JSON.stringify(method));
        this.shippingMethods.push({
          value: {
            method: newMethod.methodValue,
            price: newMethod.price,
          },
          text: newMethod.methodValue,
        });
      }
    },

    openBookReservationForm(book) {
      this.bookToReserve = book;
      this.form.quantity = 1;
      this.chosenShippingMethod = null;

      this.$bvModal.show("book-reservation-modal");
    },

    closeBookReservationForm() {
      this.$bvModal.hide("book-reservation-modal");
    },

    validForm() {
      if (
        this.form["quantity"] <= 0 ||
        this.form["quantity"] > this.bookToReserve.quantity ||
        this.chosenShippingMethod == null
      ) {
        return false;
      }
      return true;
    },

    async beginOrder() {
      this.order = {
        book: this.bookToReserve,
        quantity: parseInt(this.form.quantity), // this quantity must be taken into account, as quantity in book is in regards to total amount
        deliveryMethod: this.chosenShippingMethod,
      };

      let res = await createOrder(this.order);

      if (res.status === 200) {
        this.order.orderId = res.data.orderID;
        this.$orders.unshift(this.order);
        this.$bvToast.toast(`Successfully placed order!`, {
          title: "Order confirmation",
          autoHideDelay: 5000,
          appendToast: true,
        });
      } else {
        this.$bvToast.toast(`Failed to place order!`, {
          title: "Order confirmation",
          autoHideDelay: 5000,
          appendToast: true,
        });
      }

      this.closeBookReservationForm();
    },
  },
};
</script>

<style lang="scss" scoped></style>
